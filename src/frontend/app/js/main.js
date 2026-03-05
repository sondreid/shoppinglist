const shoppingList = {



  addItemToForm(item) {
    console.log("New item created:", item);

    if (document.getElementById(`item${item.id}`)) return;

    const container = document.getElementById('checkbox-container');
    const newItem = document.createElement('div');
    newItem.className = 'form-check d-flex align-items-center gap-2';
    
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
    }
    
    newItem.innerHTML = content;
    container.appendChild(newItem);
    
    // Refresh lightbox if available
    if (typeof refreshLightbox === 'function') {
      refreshLightbox();
    }
  }


}

