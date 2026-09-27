// Path helpers for the folder picker. Paths use "/" and may start with "~".

export const folderName = (path: string): string => path.split('/').at(-1) ?? path

export const parentFolder = (path: string): string | null => {
  const index = path.lastIndexOf('/')
  return index <= 0 ? null : path.slice(0, index)
}

export const childFolders = (folders: string[], path: string): string[] =>
  folders.filter((folder) => parentFolder(folder) === path).sort()

// "Recipe Box!" -> "recipe-box"
export const folderSlug = (name: string): string =>
  name
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')

// "recipe-box" -> "Recipe Box"
export const nameFromFolder = (path: string): string =>
  folderName(path)
    .split(/[-_\s]+/)
    .filter(Boolean)
    .map((word) => word[0]!.toUpperCase() + word.slice(1))
    .join(' ')
