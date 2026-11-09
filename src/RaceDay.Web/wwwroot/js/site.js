// Small progressive enhancements. Every page works without JavaScript.

// Image preview before upload (event banner and profile picture).
document.querySelectorAll("input[type=file][data-preview]").forEach(input => {
  input.addEventListener("change", () => {
    const target = document.getElementById(input.dataset.preview);
    const file = input.files && input.files[0];
    if (!target || !file) return;
    const url = URL.createObjectURL(file);
    if (target.tagName === "IMG") target.src = url;
    else target.innerHTML = `<img src="${url}" alt="" class="rd-avatar rd-avatar-lg">`;
  });
});

// Ask before destructive actions (delete event, delete category, cancel entry).
document.querySelectorAll("form[data-confirm]").forEach(form => {
  form.addEventListener("submit", e => {
    if (!window.confirm(form.dataset.confirm)) e.preventDefault();
  });
});

// Categories page: load a row's values into the form to edit it.
document.querySelectorAll("[data-edit-category]").forEach(btn => {
  btn.addEventListener("click", () => {
    const data = JSON.parse(btn.dataset.editCategory);
    const form = document.getElementById("category-form");
    if (!form) return;
    for (const [key, value] of Object.entries(data)) {
      const field = form.querySelector(`[name="NewCategory.${key}"]`);
      if (field) field.value = value ?? "";
    }
    document.getElementById("category-form-title").textContent = "Edit " + data.Name;
    document.getElementById("category-submit").textContent = "Save changes";
    document.getElementById("category-cancel").classList.remove("d-none");
    form.scrollIntoView({ behavior: "smooth", block: "start" });
    form.querySelector('[name="NewCategory.Name"]').focus();
  });
});

const cancelEdit = document.getElementById("category-cancel");
if (cancelEdit) {
  cancelEdit.addEventListener("click", () => {
    const form = document.getElementById("category-form");
    form.reset();
    form.querySelector('[name="NewCategory.CategoryId"]').value = "";
    document.getElementById("category-form-title").textContent = "Add a category";
    document.getElementById("category-submit").textContent = "Add category";
    cancelEdit.classList.add("d-none");
  });
}

// Results entry: time and position only apply to finishers.
document.querySelectorAll("form.result-form").forEach(form => {
  const status = form.querySelector("[name=Status]");
  const toggle = () => {
    const finished = status.value === "Finished";
    form.querySelectorAll("[name=FinishTime],[name=Position]").forEach(f => { f.disabled = !finished; if (!finished) f.value = ""; });
  };
  status.addEventListener("change", toggle);
  toggle();
});
