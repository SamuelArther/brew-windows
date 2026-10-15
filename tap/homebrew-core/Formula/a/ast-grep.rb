class AstGrep < Formula
  desc "Code searching, linting, rewriting"
  homepage "https://ast-grep.github.io/"
  url "https://github.com/ast-grep/ast-grep/releases/download/0.51.0/app-x86_64-pc-windows-msvc.zip"
  sha256 "1a5b7824c5925ac34257f806488e54860817cefd855fd308d13df7550d068908"
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
