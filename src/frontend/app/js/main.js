const shoppingList = {



  addItemToForm(item) {

    if (document.getElementById(`item${item.id}`)) return;

    const container = document.getElementById('checkbox-container');
    const newItem = document.createElement('div');
    newItem.className = 'form-check d-flex align-items-center gap-2';
    newItem.dataset.qty = item.quantity || 1;
    
    let content = `
      <input type="checkbox" class="form-check-input" id="item${item.id}" onchange="updateItem(${item.id})">
    `;
    
    if (item.isImage && item.image) {
      const dataUrl = `data:${item.image.contentType};base64,${item.image.imageBinary}`;
      content += `
        <a href="${dataUrl}" class="glightbox" data-gallery="gallery1">
          <img src="${dataUrl}" alt="${item.name || 'Image'}" style="max-width: 60px; max-height: 60px; cursor: pointer;">
        </a>
      `;
    }
    
    if (item.name) {
      content += `<label class="form-check-label" for="item${item.id}">${item.name}</label>`;
      content += `
        <span class="qty-controls">
          <button type="button" class="qty-btn" aria-label="One less" onclick="changeQty(${item.id}, -1)">−</button>
          <span class="qty-value" id="qty${item.id}">${item.quantity || 1}</span>
          <button type="button" class="qty-btn" aria-label="One more" onclick="changeQty(${item.id}, 1)">+</button>
        </span>
      `;
    }
    
    newItem.innerHTML = content;
    // Newest on top. Initial load iterates items oldest-first, so
    // prepending yields newest-first there too, matching live adds.
    container.prepend(newItem);
    
    // Refresh lightbox if available
    if (typeof refreshLightbox === 'function') {
      refreshLightbox();
    }
  }


}

