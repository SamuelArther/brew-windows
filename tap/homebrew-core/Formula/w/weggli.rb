class Weggli < Formula
  desc "Fast and robust semantic search tool for C and C++ codebases"
  homepage "https://github.com/weggli-rs/weggli"
  url "https://github.com/weggli-rs/weggli/archive/refs/tags/v0.2.4.tar.gz"
  sha256 "12fde9a0dca2852d5f819eeb9de85c4d11c5c384822f93ac66b2b7b166c3af78"
  license "Apache-2.0"
  head "https://github.com/weggli-rs/weggli.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "aca8d2634fdb0ce49a66de391cbbedd490353eea8c0d98dd017160e0dbf6f28e"
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
    (testpath/"test.c").write("void foo() {int bar=10+foo+bar;}")
    system bin/"weggli", "{int $a = _+foo+$a;}", testpath/"test.c"
  end
end
