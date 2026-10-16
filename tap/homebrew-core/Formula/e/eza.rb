class Eza < Formula
  desc "Modern, maintained replacement for ls"
  homepage "https://eza.rocks"
  url "https://github.com/eza-community/eza/releases/download/v0.23.5/eza.exe_x86_64-pc-windows-gnu.zip"
  sha256 "c830638c844a5b89d39ba662b5549903a71fa539018e813880f5b8afa77bac2e"
  license "EUPL-1.2"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/eza.exe", "eza"
  end

  test do
    testfile = "test.txt"
    touch testfile
    # The Windows build lists nothing when given no path, so name the directory.
    assert_match testfile, shell_output("#{bin}/eza .")
  end
end
