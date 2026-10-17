class GoStatik < Formula
  desc "Embed files into a Go executable"
  homepage "https://github.com/rakyll/statik"
  url "https://github.com/rakyll/statik/archive/refs/tags/v0.1.8.tar.gz"
  sha256 "670d14b003fb883efbd68f3e813b1022ce2325a6b4b6d2e9e6445f38db51f902"
  license "Apache-2.0"
  head "https://github.com/rakyll/statik.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "6d16aac0f6926aff398774b52a398f55784eb2ef04d694aac153ba3d1efef8c7"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    system "go", "build", *std_go_args(output: bin/"statik")
  end

  test do
    font_path = if OS.mac?
      "/Library/Fonts/Arial Unicode.ttf"
    elsif OS.windows?
      "C:/Windows/Fonts/arial.ttf"
    else
      "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
    end
    system bin/"statik", "-src", font_path
    assert_path_exists testpath/"statik/statik.go"
    refute_predicate (testpath/"statik/statik.go").size, :zero?
  end
end
