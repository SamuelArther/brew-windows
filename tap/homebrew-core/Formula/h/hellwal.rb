class Hellwal < Formula
  desc "Fast, extensible color palette generator"
  homepage "https://github.com/danihek/hellwal"
  url "https://github.com/danihek/hellwal/archive/refs/tags/1.0.8.tar.gz"
  sha256 "53f629f22bd80c95150fa8510c4c5f4969beec06bfd01ad33a5fdad3d56a357e"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/hellwal-1.0.8"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "4257d05f22812666d7b8471ed8f2e9502078de48fc022865619bd2e68b47cceb"
  end

  def install
    system "make", "install", "DESTDIR=#{bin}"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hellwal --version")

    (testpath/"hw.theme").write "%% color0  = #282828 %%"
    output = shell_output("#{bin}/hellwal --skip-term-colors -j -t hw.theme 2>&1", 1)
    assert_match "Not enough colors were specified in color palette", output
  end
end
