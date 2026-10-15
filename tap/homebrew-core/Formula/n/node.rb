class Node < Formula
  desc "JavaScript runtime built on V8"
  homepage "https://nodejs.org/"
  url "https://nodejs.org/dist/v26.12.0/node-v26.12.0-win-x64.zip"
  version "26.12.0"
  sha256 "50efaf01a08c520b4bbe499c09a8c5a5c370002c419c0d9da2c6815d3066a453"
  license "MIT"

  def install
    install_tree
    windows_exe "libexec/node.exe", "node"
    windows_cmd "libexec/npm.cmd", "npm"
    windows_cmd "libexec/npx.cmd", "npx"
  end
end
