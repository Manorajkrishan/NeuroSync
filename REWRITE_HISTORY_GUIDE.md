# 🔧 Rewrite Git History to Remove Secrets

## ⚠️ WARNING

This will **rewrite Git history**. Only do this if:
- ✅ You're the only one using this repository, OR
- ✅ You've coordinated with all collaborators
- ✅ You understand that this changes commit hashes
- ✅ You're okay with force-pushing

## 🎯 The Problem

Secrets are in commit `4c5bd6c6349c1adaf4f0d7a8d68e9c05f6a54a89` in these files:
- `YOUTUBE_TOKENS_ADDED.md`
- `YOUTUBE_SETUP.md`
- `QUICK_ENABLE_DEVICES.md`
- `NeuroSync.Api/bin/Debug/net8.0/appsettings.json`

## ✅ Solution: Rewrite History

### Option 1: Use git filter-repo (Recommended, but requires installation)

```powershell
# Install git-filter-repo (if not installed)
# pip install git-filter-repo

# Remove secrets from all commits
git filter-repo --path YOUTUBE_TOKENS_ADDED.md --path YOUTUBE_SETUP.md --path QUICK_ENABLE_DEVICES.md --path NeuroSync.Api/bin/Debug/net8.0/appsettings.json --invert-paths
```

### Option 2: Use git filter-branch (Built-in, but slower)

```powershell
# Remove secrets from all commits using filter-branch
git filter-branch --force --index-filter "git rm --cached --ignore-unmatch YOUTUBE_TOKENS_ADDED.md YOUTUBE_SETUP.md QUICK_ENABLE_DEVICES.md 'NeuroSync.Api/bin/Debug/net8.0/appsettings.json'" --prune-empty --tag-name-filter cat -- --all

# Force push
git push origin --force --all
```

### Option 3: Start Fresh Branch (Easiest)

```powershell
# Create a new branch from the beginning
git checkout --orphan clean-master

# Add all current files (without history)
git add .

# Commit
git commit -m "Initial commit - secrets removed"

# Force push
git push origin clean-master --force

# Delete old master locally
git branch -D master

# Rename clean-master to master
git branch -m master

# Force push master
git push origin master --force
```

## 🚀 Recommended Approach

Since this appears to be a new project, **Option 3 (Start Fresh Branch)** is easiest and safest.

## 📝 Steps for Option 3

1. **Create clean branch:**
   ```powershell
   git checkout --orphan clean-master
   ```

2. **Add all files:**
   ```powershell
   git add .
   ```

3. **Remove temporary files (optional):**
   ```powershell
   git rm --cached GIT_*.md IMPORTANT_DOCS.md COMMIT_READY.md 2>$null
   ```

4. **Commit:**
   ```powershell
   git commit -m "Initial commit - secrets removed"
   ```

5. **Push clean branch:**
   ```powershell
   git push origin clean-master --force
   ```

6. **Make it master:**
   ```powershell
   git branch -D master
   git branch -m master
   git push origin master --force
   ```

## ⚠️ Important Notes

- **This rewrites history** - commit hashes will change
- **Force push required** - this overwrites remote history
- **Coordinators needed** - anyone else using the repo needs to reset
- **Backup first** - make sure you have a backup
