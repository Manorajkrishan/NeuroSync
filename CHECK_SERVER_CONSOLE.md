# Critical: Check Server Console for Error!

## The 500 Error is Happening

You're getting a 500 error, which means something is failing on the server side.

## IMPORTANT: Look at Your Server Console!

**The error message will be in the terminal/console where you ran `dotnet run`!**

Look for:
- Red error messages
- "Error detecting emotion" messages
- Stack traces
- "CRITICAL: Failed to initialize" messages

## What's Likely Happening

Since the model doesn't exist, it needs to train on first use. The training might be failing. The error details will be in the **server console**, not the browser.

## Next Steps

1. **Look at your server console** (the terminal where you ran `dotnet run`)
2. **Copy the error message** you see there
3. **Common errors:**
   - "Failed to load model"
   - "Training failed"
   - "File not found"
   - ML.NET specific errors

## The Error is NOT in the Browser

The browser just shows "500 Internal Server Error", but the **actual error details** are in the server console where `dotnet run` is running.

**Please check that console and share what error messages you see there!**

