class Geni < Formula
  desc "Standalone database migration tool"
  homepage "https://github.com/emilpriver/geni"
  url "https://github.com/emilpriver/geni/archive/refs/tags/v1.3.3.tar.gz"
  sha256 "4b12e63bf71e283381d923f108f1a48e52fe7e16ffcb21a082d2deb89e74e023"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "43a9c20850e9dac50b30ca64917425b12366d89572f20e0dd349d06262789e90"
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
    ENV["DATABASE_URL"] = "sqlite3://test.sqlite"
    system bin/"geni", "create"
    assert_path_exists testpath/"test.sqlite", "failed to create test.sqlite"
    assert_match version.to_s, shell_output("#{bin}/geni --version")
  end
end
