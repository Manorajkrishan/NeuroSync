# How to See the Actual Error in Browser

## Quick Method: Run This in Browser Console

1. **Open Browser Console**: Press F12, go to Console tab
2. **Copy and paste this code**:
```javascript
fetch('http://localhost:5063/api/emotion/detect', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ text: "I'm happy!" })
})
.then(async response => {
  const text = await response.text();
  console.log('=== ERROR RESPONSE ===');
  console.log('Status:', response.status);
  console.log('Response:', text);
  try {
    const json = JSON.parse(text);
    console.log('Parsed:', json);
    if (json.error) console.log('ERROR:', json.error);
    if (json.details) console.log('DETAILS:', json.details);
  } catch (e) {
    console.log('Could not parse as JSON');
  }
})
.catch(error => {
  console.error('Fetch error:', error);
});
```

3. **Press Enter**
4. **Look at the console output** - it will show the actual error message!

---

## Alternative: Check Network Tab Response

1. **Open Developer Tools** (F12)
2. **Go to Network tab**
3. **Click "Detect Emotion"** in your app
4. **Click on the request** named `emotion/detect`
5. **Click on "Response" tab** (next to Headers)
6. **You should see the error JSON** there

---

## What You Should See

The response should look like:
```json
{
  "error": "An error occurred: [actual error message here]",
  "details": "[stack trace here]"
}
```

**Copy that error message and share it with me!**

---

## Why This Matters

The browser Network tab only shows "500 Internal Server Error" in the status, but the **Response tab** shows the actual error details that the server returned.

This will tell us exactly what's failing!

