class Octobuild < Formula
  desc "Compiler cache for Unreal Engine"
  homepage "https://github.com/octobuild/octobuild"
  url "https://github.com/octobuild/octobuild/archive/refs/tags/2.0.0.tar.gz"
  sha256 "ff6c54184351fb03e1997c59db98eec705a5cea2969d72d220f50110d4c34853"
  license "MIT"
  head "https://github.com/octobuild/octobuild.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b73680052b87e8f8fbed274c44bbf5f1ca800b44aa63b5988e7234c12c01b0ed"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  on_linux do
    depends_on "openssl@4"
  end

  deny_network_access!

  def fetch
    # `Cargo.lock` file is ignored
    system "cargo", "generate-lockfile"
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    ENV["OPENSSL_DIR"] = dep("openssl").opt_prefix if OS.linux?
    system "cargo", "install", *std_cargo_args
  end

  test do
    output = shell_output bin/"xgConsole"
    assert_match "Current configuration", output
    assert_match "cache_limit_mb", output
  end
end
