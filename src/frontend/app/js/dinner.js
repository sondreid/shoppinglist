const dinnerPlanner = {

  monday: null,
  dayNames: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'],

  show() {
    if (!this.monday) this.monday = this.getMonday(new Date());
    this.loadWeek();
  },

  changeWeek(direction) {
    this.monday = this.addDays(this.monday, direction * 7);
    this.loadWeek();
  },

  getMonday(date) {
    const d = new Date(date);
    d.setHours(0, 0, 0, 0);
    const day = d.getDay(); // 0 = Sunday
    d.setDate(d.getDate() - (day === 0 ? 6 : day - 1));
    return d;
  },

  addDays(date, days) {
    const d = new Date(date);
    d.setDate(d.getDate() + days);
    return d;
  },

  toDateString(date) {
    const pad = n => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
  },

  loadWeek() {
    const from = this.toDateString(this.monday);
    const to = this.toDateString(this.addDays(this.monday, 6));

    return fetch(`${auth.getApiBase()}/dinnerplans?from=${from}&to=${to}`, {
      method: 'GET',
      headers: auth.getAuthHeaders()
    })
      .then(handleJson)
      .then(plans => this.renderWeek(plans))
      .catch((error) => {
        console.error('Error:', error);
      });
  },

  renderWeek(plans) {
    const sunday = this.addDays(this.monday, 6);
    document.getElementById('week-label').textContent =
      `${this.monday.getDate()} ${this.monday.toLocaleString('en', { month: 'short' })} – ` +
      `${sunday.getDate()} ${sunday.toLocaleString('en', { month: 'short' })} ${sunday.getFullYear()}`;

    const container = document.getElementById('dinner-days');
    container.replaceChildren();

    const todayString = this.toDateString(new Date());

    for (let i = 0; i < 7; i++) {
      const date = this.addDays(this.monday, i);
      const dateString = this.toDateString(date);
      const plan = plans.find(p => p.date === dateString);

      const dayDiv = document.createElement('div');
      dayDiv.className = 'dinner-day' + (dateString === todayString ? ' today' : '');

      let content = `
        <div class="dinner-day-header">${this.dayNames[i]} <span class="dinner-day-date">${date.getDate()} ${date.toLocaleString('en', { month: 'short' })}</span></div>
        <input type="text" class="form-control dinner-recipe-input" placeholder="What's for dinner?"
          onkeypress="if(event.key === 'Enter') this.blur();"
          onblur="dinnerPlanner.saveRecipe('${dateString}', this)">
      `;

      if (plan) {
        content += '<div class="dinner-ingredients">';
        for (const ingredient of plan.ingredients) {
          content += `
            <div class="dinner-ingredient d-flex align-items-center gap-2">
              <span></span>
              <button type="button" class="delete-completed" aria-label="Remove" onclick="dinnerPlanner.deleteIngredient(${ingredient.id})">🗑</button>
            </div>
          `;
        }
        content += `
          <div class="input-row">
            <input type="text" class="form-control dinner-ingredient-input" placeholder="Add ingredient" data-plan-id="${plan.id}"
              onkeypress="if(event.key === 'Enter') dinnerPlanner.addIngredient(${plan.id}, this);">
            <div class="input-actions">
              <button type="button" class="btn btn-primary" onclick="dinnerPlanner.addIngredient(${plan.id}, this.closest('.input-row').querySelector('.dinner-ingredient-input'))">Add</button>
            </div>
          </div>
        </div>`;
      }

      dayDiv.innerHTML = content;

      // Set texts via textContent/value so names with quotes render safely
      const recipeInput = dayDiv.querySelector('.dinner-recipe-input');
      recipeInput.value = plan?.recipe || '';
      recipeInput.dataset.original = recipeInput.value;
      if (plan) {
        dayDiv.querySelectorAll('.dinner-ingredient span').forEach((span, index) => {
          span.textContent = plan.ingredients[index].name;
        });
      }

      container.appendChild(dayDiv);
    }
  },

  saveRecipe(dateString, input) {
    const recipe = input.value.trim();
    if (recipe === input.dataset.original) return;

    fetch(`${auth.getApiBase()}/dinnerplan`, {
      method: 'POST',
      headers: auth.getAuthHeaders(),
      body: JSON.stringify({ date: dateString, recipe: recipe || null })
    })
      .then(handleJson)
      .then(() => this.loadWeek())
      .catch((error) => {
        console.error('Error:', error);
      });
  },

  addIngredient(planId, input) {
    const name = input.value.trim();
    if (!name) return;

    fetch(`${auth.getApiBase()}/dinnerplan/${planId}/ingredient`, {
      method: 'POST',
      headers: auth.getAuthHeaders(),
      body: JSON.stringify({ name: name })
    })
      .then(handleJson)
      .then(() => this.loadWeek())
      .then(() => {
        // Keep focus so several ingredients can be added in a row
        document.querySelector(`.dinner-ingredient-input[data-plan-id="${planId}"]`)?.focus();
      })
      .catch((error) => {
        console.error('Error:', error);
      });
  },

  deleteIngredient(id) {
    fetch(`${auth.getApiBase()}/dinneringredient/${id}`, {
      method: 'DELETE',
      headers: auth.getAuthHeaders()
    })
      .then(handleJson)
      .then(() => this.loadWeek())
      .catch((error) => {
        console.error('Error:', error);
      });
  }

}
