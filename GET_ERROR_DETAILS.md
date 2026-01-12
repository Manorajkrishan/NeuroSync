# How to Get the Actual Error Details

## The Problem

You're seeing "500 Internal Server Error" but need to know **WHY** it's failing.

## Method 1: Check Server Console (BEST)

**Look at the terminal where you ran `dotnet run`**

When you click "Detect Emotion" in the browser, immediately look at that terminal window. You should see red error messages.

**Please copy and paste those error messages here!**

## Method 2: Check Browser Network Tab Response

1. Open Browser Developer Tools (F12)
2. Go to **Network** tab
3. Click "Detect Emotion" in your app
4. Click on the request: `api/emotion/detect`
5. Go to **Response** tab (not Headers)
6. You should see the error details there

The response should show something like:
```json
{
  "error": "An error occurred: [actual error message]",
  "details": "[stack trace]"
}
```

## Method 3: Test Directly

Open browser console (F12 → Console) and run:

```javascript
fetch('http://localhost:5063/api/emotion/detect', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ text: "I'm happy" })
})
.then(async r => {
  const text = await r.text();
  console.log('Status:', r.status);
  console.log('Response:', text);
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
})
.then(console.log)
.catch(console.error)
```

This will show you the actual error response.

---

## What I Need From You

**Please check your SERVER console** (where you ran `dotnet run`) and share:

1. Any error messages you see there
2. Any red text
3. Stack traces

**OR** check the browser Network tab Response section and share what you see there.

The 500 error is happening, but I need to see the **actual error message** to fix it!

