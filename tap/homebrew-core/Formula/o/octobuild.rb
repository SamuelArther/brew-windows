class Octobuild < Formula
  desc "Compiler cache for Unreal Engine"
  homepage "https://github.com/octobuild/octobuild"
  url "https://github.com/octobuild/octobuild/archive/refs/tags/2.0.0.tar.gz"
  sha256 "ff6c54184351fb03e1997c59db98eec705a5cea2969d72d220f50110d4c34853"
  license "MIT"
  head "https://github.com/octobuild/octobuild.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "84e12201d26d04c9b679472b6804018b8b2ab058c00c8d8de171a68ab76a3773"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  on_linux do
    depends_on "openssl@4"
  end

  def install
    ENV["OPENSSL_DIR"] = formula_opt_prefix("openssl@4") if OS.linux?
    system "cargo", "install", *std_cargo_args
  end

  test do
    output = shell_output bin/"xgConsole"
    assert_match "Current configuration", output
    assert_match "cache_limit_mb", output
  end
end
