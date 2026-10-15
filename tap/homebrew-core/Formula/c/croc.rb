class Croc < Formula
  desc "Securely send things from one computer to another"
  homepage "https://github.com/schollz/croc"
  url "https://github.com/schollz/croc/releases/download/v11.5.4/croc_v11.5.4_Windows-64bit.zip"
  sha256 "a75e1bac08948e21a8f02a39ca387b5b516dc91c00b6358e3c14b9726f1992b7"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/croc.exe", "croc"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/croc --version")
  end
end
