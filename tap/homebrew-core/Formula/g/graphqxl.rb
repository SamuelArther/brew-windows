class Graphqxl < Formula
  desc "Language for creating big and scalable GraphQL server-side schemas"
  homepage "https://gabotechs.github.io/graphqxl"
  url "https://github.com/gabotechs/graphqxl/archive/refs/tags/v0.40.2.tar.gz"
  sha256 "2ddc205e3943e97273984e64bdac3d41c70b87eb408976d7d2108163fca35d4e"
  license "MIT"
  head "https://github.com/gabotechs/graphqxl.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "82bd5cd2e04dbf6c77736416984a46b08204109a2d28913a1c5dfdfacf4f2587"
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
    test_file = testpath/"test.graphqxl"
    test_file.write "type MyType { foo: String! }"
    system bin/"graphqxl", test_file
    assert_equal "type MyType {\n  foo: String!\n}\n\n", (testpath/"test.graphql").read
  end
end
