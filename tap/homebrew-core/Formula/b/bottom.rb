class Bottom < Formula
  desc "Yet another cross-platform graphical process/system monitor"
  homepage "https://clementtsang.github.io/bottom/"
  url "https://github.com/ClementTsang/bottom/releases/download/0.14.9/bottom_x86_64-pc-windows-msvc.zip"
  sha256 "01660721b0fdc72b191f2b27b78dc9a3087aeb7b3fad4a890c53870089d6b500"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/btm.exe", "btm"
  end

  test do
    assert_match "bottom #{version}", shell_output("#{bin}/btm --version")
  end
end
