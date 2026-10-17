class Oak < Formula
  desc "Expressive, simple, dynamic programming language"
  homepage "https://oaklang.org/"
  url "https://github.com/thesephist/oak/archive/refs/tags/v0.3.tar.gz"
  sha256 "05bc1c09da8f8d199d169e5a5c5ab2f2923bad6fac624f497f5ea365f378e38a"
  license "MIT"
  head "https://github.com/thesephist/oak.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "417354c9256912aa471c275259b30dd98df04c5597cc92778ff0c76ff728d34b"
  end

  depends_on "go" => :build

  conflicts_with "oakc", because: "both install `oak` binaries"

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    assert_equal "Hello, World!\n14\n", shell_output("#{bin}/oak eval \"std.println('Hello, World!')\"")
  end
end
