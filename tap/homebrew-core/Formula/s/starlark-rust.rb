class StarlarkRust < Formula
  desc "Rust implementation of the Starlark language"
  homepage "https://github.com/facebook/starlark-rust"
  url "https://github.com/facebook/starlark-rust/archive/refs/tags/v0.14.0.tar.gz"
  sha256 "3a0030b4fe1becf62698a6aed0dbbbd68397e7406fa0eca078849622b93d56e4"
  license "Apache-2.0"
  head "https://github.com/facebook/starlark-rust.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "01326c737143d08f55525b1560e23345836aa3e95d966729cf73234be1951636"
  end

  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args(path: "starlark_bin")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/starlark --version")

    (testpath/"test.bzl").write <<~BAZEL
      def hello_world():
          print("Hello, world!")
      hello_world()
    BAZEL

    output = shell_output("#{bin}/starlark --check test.bzl")
    assert_equal "1 files, 0 errors, 0 warnings, 0 advices, 0 disabled", output.chomp
  end
end
