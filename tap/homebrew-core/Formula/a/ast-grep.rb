class AstGrep < Formula
  desc "Code searching, linting, rewriting"
  homepage "https://ast-grep.github.io/"
  version "0.50.0"
  url "https://github.com/ast-grep/ast-grep/releases/download/0.50.0/app-x86_64-pc-windows-msvc.zip"
  sha256 "e332ce9b10731c6d9bf142ed5ce2ab3795a8715e434dcca77de18e3e0f86e3f5"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/ast-grep.exe", "ast-grep"
    windows_exe "libexec/sg.exe", "sg"
  end

  test do
    (testpath/"hi.js").write("console.log('it is me')")
    system bin/"ast-grep", "run", "-l", "js", "-p", "console.log", testpath/"hi.js"
  end
end
