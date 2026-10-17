class Dipc < Formula
  desc "Convert your favorite images/wallpapers with your favorite color palettes/themes"
  homepage "https://github.com/doprz/dipc"
  url "https://github.com/doprz/dipc/archive/refs/tags/v1.2.0.tar.gz"
  sha256 "3cc4e46cb6531b4b903d2c1a209c19667acac34d91caff5967600ed51c4bebc0"
  license any_of: ["MIT", "Apache-2.0"]

  bottle do
    sha256 cellar: :any, x86_64_windows: "8fcff920546e6b06d1a2390007eee46d617f7fdcbc2cda8b94ba1746dfae2ccd"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    # Test processing with a built-in theme (gruvbox)
    output_path = testpath/"output.png"
    system bin/"dipc", "gruvbox", "--styles", "Dark mode", "-o", output_path, test_fixtures("test.png")

    assert_path_exists output_path
    assert_operator output_path.size, :>, 0

    assert_match version.to_s, shell_output("#{bin}/dipc --version")
  end
end
