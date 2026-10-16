class Svu < Formula
  desc "Semantic version utility"
  homepage "https://github.com/caarlos0/svu"
  url "https://github.com/caarlos0/svu/releases/download/v3.4.1/svu_3.4.1_windows_amd64.zip"
  sha256 "7f8d17beb8fbe4a5c6e232072e93a71c8338173dafd95eda898bd1838d12b14e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/caarlos0/svu/archive/refs/tags/v3.4.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/svu.exe", "svu"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/svu --version")
    system bin/"svu", "init"
    assert_match "svu configuration", (testpath/".svu.yml").read
  end
end
