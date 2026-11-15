class StarlarkRust < Formula
  desc "Rust implementation of the Starlark language"
  homepage "https://github.com/facebook/starlark-rust"
  url "https://static.crates.io/crates/starlark_bin/starlark_bin-0.14.0.crate"
  sha256 "0c33c0ca1ebd9da2f735029e3815a49a593273567315a0e5b22dff4d2899f196"
  license "Apache-2.0"
  head "https://github.com/facebook/starlark-rust.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "deb7d902f298dff0c33bca8e34aae3d2e16992791a5b4158f3e1b76372725644"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "generate-lockfile" if build.head?
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: build.head? ? "starlark_bin" : ".")
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
