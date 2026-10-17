class CouchbaseShell < Formula
  desc "Modern and fun shell for Couchbase Server and Capella"
  homepage "https://couchbase.sh"
  url "https://github.com/couchbaselabs/couchbase-shell/archive/refs/tags/v1.4.0.tar.gz"
  sha256 "9300027b9d9d20904ed0306e5a7fe745f3bba8519d17e0f743cab52f3c574fa9"
  license "Apache-2.0"
  head "https://github.com/couchbaselabs/couchbase-shell.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "60b1757a1befc379720430edfa9f45196248bef91b09a94dee5d4230e7468581"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  on_linux do
    depends_on "libx11"
    depends_on "libxcb"
    depends_on "openssl@4"
    depends_on "zlib-ng-compat"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match "homebrew_test", shell_output("#{bin}/cbsh -c '{ foo: 1, bar: homebrew_test} | get bar'")
  end
end
