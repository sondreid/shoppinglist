const shoppingList = {

  addItem() {
    itemCount++;
    const container = document.getElementById('checkbox-container');
    const newItemInput = document.getElementById('newItemInput');
    const itemText = newItemInput.value;

    if (!itemText.trim()) return; // Don't add empty items

    const newItem = document.createElement('div');
    newItem.className = 'form-check';
    newItem.innerHTML = `
      <input type="checkbox" class="form-check-input" id="item${itemCount}">
      <label class="form-check-label" for="item${itemCount}">${itemText}</label>
      `;
    container.appendChild(newItem);

    fetch('http://localhost:5058/storeitem', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        item: itemText
      })
    })
      .then(response => response.json())
      .then(data => {
        console.log('Success:', data);
        newItemInput.value = ''; // Clear input after successful add
      })
      .catch((error) => {
        console.error('Error:', error);
      });
  },


  addItemToForm(item) {
    console.log("New item created:", item);

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

