﻿using Jedznaplus.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Jedznaplus.Controllers
{
    public class RecipesController : Controller
    {
        //
        // GET: /Recipes/

        private DatabaseModel db = new DatabaseModel();
        protected ApplicationDbContext ApplicationDbContext { get; set; }
        protected UserManager<ApplicationUser> UserManager { get; set; }
        private SelectList UnitNameList;
        private SelectList Difficulties;

        public RecipesController()
        {
            this.ApplicationDbContext = new ApplicationDbContext();
            this.UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(this.ApplicationDbContext));
            UnitNameList = new SelectList(new[] { "Litr", "Mililitr", "Kilogram", "Dekagram", "Gram", "Sztuka", "Plaster", "Opakowanie", "Łyżka", "Łyżeczka", "Szklanka" });
            Difficulties = new SelectList(new[] { "Łatwy", "Średni", "Trudny", "Bardzo Trudny"});
        }

        public ActionResult Index()
        {
            var recipes = db.Recipes.ToList();

            return View(recipes);
        }


        public void deleteImg(string relativePath)
        {
            if (relativePath != "~/Images/noPhoto.png")
            {
                var path = Server.MapPath(relativePath);
                System.IO.File.Delete(path);
            }
        }

        [Authorize]
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.Difficulties = Difficulties;
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult Create(Models.Recipe recipe, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                recipe.UserName = User.Identity.Name;

                if (file != null && file.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var uniqueFileName = Guid.NewGuid() + fileName;
                    var absolutePath = Path.Combine(Server.MapPath("~/Images"), uniqueFileName);
                    var relativePath = "~/Images/" + uniqueFileName;
                    file.SaveAs(absolutePath);
                    recipe.ImageUrl = relativePath;
                }
                else
                {
                    recipe.ImageUrl = "~/Images/noPhoto.png";
                }

                return RedirectToAction("CreateAddIngredients", recipe);
            }
            ViewBag.Difficulties = Difficulties;
            return Create();
        }

        [HttpGet]
        public ActionResult CreateAddIngredients(Recipe recipe)
        {
            return View(recipe);
        }

        [HttpPost]
        [ActionName("CreateAddIngredients")]
        public ActionResult CreateAddIngredientsPost(Recipe recipe)
        {
            if (recipe.PreparationMethod != null && recipe.Ingredients != null)
            {
                db.Recipes.Add(recipe);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "Przepis musi awierać listę składników oraz sposób przygotowania");
            return RedirectToAction("CreateAddIngredients", recipe);
        }



        [Authorize]
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var recipe = db.Recipes.Find(id);
            ViewBag.UnitNameList = UnitNameList;
            ViewBag.Difficulties = Difficulties;
            
            return View(recipe);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Edit(Models.Recipe recipe, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                var dbPost = db.Recipes.FirstOrDefault(p => p.Id == recipe.Id);
                if (dbPost == null)
                {
                    return HttpNotFound();
                }

                dbPost.Calories = recipe.Calories;
                
                if (dbPost.Ingredients.Count != recipe.Ingredients.Count && dbPost.Ingredients.Distinct().Count() != recipe.Ingredients.Count && !(dbPost.Ingredients.All(item => recipe.Ingredients.Contains(item))))
                {
                db.Ingredient.RemoveRange(dbPost.Ingredients);
                dbPost.Ingredients = new List<Ingredient>(recipe.Ingredients);
                }
                
                dbPost.Name = recipe.Name;
                dbPost.PreparationMethod = recipe.PreparationMethod;
                dbPost.PreparationTime = recipe.PreparationTime;
                dbPost.Serves = recipe.Serves;
                dbPost.Difficulty = recipe.Difficulty;

                if (file != null && file.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var uniqueFileName = Guid.NewGuid() + fileName;
                    var absolutePath = Path.Combine(Server.MapPath("~/Images"), uniqueFileName);
                    var relativePath = "~/Images/" + uniqueFileName;
                    file.SaveAs(absolutePath);
                    dbPost.ImageUrl = relativePath;
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.UnitNameList = UnitNameList;
            ViewBag.Difficulties = Difficulties;
            return View(recipe);

        }


        [Authorize]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var toDelete = db.Recipes.Find(id);

            if (toDelete != null)
            {
                var votes = db.VoteLogs.Where(p => p.VoteForId == toDelete.Id);
                deleteImg(toDelete.ImageUrl);
               
                db.Comments.RemoveRange(toDelete.Comments);
                db.Ingredient.RemoveRange(toDelete.Ingredients);               
                db.VoteLogs.RemoveRange(votes);
                db.Recipes.Remove(toDelete);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [Authorize]
        public ActionResult UserRecipes()
        {
            var userRecipes = db.Recipes.Where(a => a.UserName == User.Identity.Name).ToList();

            return View(userRecipes);
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var toView = db.Recipes.Find(id);

            if (toView != null)
            {
                ViewBag.AvatarURL = UserManager.FindByName(toView.UserName).AvatarUrl;
                return View(toView);
            }

            return RedirectToAction("Index");
        }

        public ActionResult DeleteImage(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var toDelete = db.Recipes.Find(id);

            if (toDelete != null)
            {
                deleteImg(toDelete.ImageUrl);
                toDelete.ImageUrl = "~/Images/noPhoto.png";
                db.SaveChanges();
            }

            return RedirectToAction("Edit", db.Recipes.Find(id));

        }

        public ActionResult Search(string search)
        {
            var recipes = db.Recipes.Where(a => a.Name.ToLower().Contains(search.ToLower())).ToList();
            var allRecipes = db.Recipes.ToList();

            foreach (var rec in allRecipes)
            {
                foreach (var ingred in rec.Ingredients)
                {
                    if (ingred.Name.IndexOf(search.ToLower(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!recipes.Contains(rec))
                            recipes.Add(rec);
                    }
                }
            }

            return View(recipes);
        }

        [HttpGet]
        public ActionResult AdvancedSearch()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AdvancedSearch(int? Time, string WithoutIngred, bool? Vegetarians)
        {
            if (Time == null) Time = 1000;
            if (Vegetarians == null) Vegetarians = false;
            if (string.IsNullOrEmpty(WithoutIngred)) WithoutIngred = "testowy string";

            var recipes = new List<Recipe>();
            var allRecipes = db.Recipes.ToList();

            foreach (var rec in allRecipes)
            {
                foreach (var ingred in rec.Ingredients)
                {
                    if (rec.PreparationTime <= Time && (ingred.Name.IndexOf(WithoutIngred.ToLower(), StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        recipes.Add(rec);
                    }

                }

            }
            return View("Search", recipes);
        }

        public string CountVotes(string votesString)
        {
            Single m_Average = 0;

            Single m_totalNumberOfVotes = 0;
            Single m_totalVoteCount = 0;
            Single m_currentVotesCount = 0;
            Single m_inPercent = 0;

            // calculate total votes now
            string[] votes = votesString.Split(',');
            for (int i = 0; i < votes.Length; i++)
            {
                m_currentVotesCount = int.Parse(votes[i]);
                m_totalNumberOfVotes = m_totalNumberOfVotes + m_currentVotesCount;
                m_totalVoteCount = m_totalVoteCount + (m_currentVotesCount * (i + 1));
            }

            m_Average = m_totalVoteCount / m_totalNumberOfVotes;
            m_inPercent = (m_Average * 100) / 5;

            return "<span style=\"display: block; width: 70px; height: 13px; background: url(/Images/whitestar.gif) 0 0;\">" +
                  "<span style=\"display: block; width: " + m_inPercent + "%; height: 13px; background: url(/Images/yellowstar.gif) 0 -13px;\"></span> " +
                  "</span>" +
                  "<span class=\"smallText\">Ilość głosów: <span itemprop=\"ratingCount\">" + m_totalNumberOfVotes + "</span> | Średnia ocen : <span itemprop=\"ratingValue\">" + m_Average.ToString("##.##") + "</span> na 5 </span>  ";

        }

        public ActionResult IngredientEntryRow()
        {
            ViewBag.UnitNames = UnitNameList;
            return PartialView("IngredientEditor");
        }



    }
}
