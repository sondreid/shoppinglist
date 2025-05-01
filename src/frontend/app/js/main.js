function postItem() {
  fetch('http://localhost:5058/storeitem', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      item: 'New Item'
    })
  })
    .then(response => response.json())
    .then(data => {
      console.log('Success:', data);
    })
    .catch((error) => {
      console.error('Error:', error);
    });
}


function addItem() {
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
};
