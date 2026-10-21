class StarlarkRust < Formula
  desc "Rust implementation of the Starlark language"
  homepage "https://github.com/facebook/starlark-rust"
  url "https://github.com/facebook/starlark-rust/archive/refs/tags/v0.14.0.tar.gz"
  sha256 "3a0030b4fe1becf62698a6aed0dbbbd68397e7406fa0eca078849622b93d56e4"
  license "Apache-2.0"
  head "https://github.com/facebook/starlark-rust.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "5786d9517476d1f728eb7b1c918d9e294592487234d1a1234fb8d595a1954085"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    # `https://github.com/facebookexperimental/rust-shed.git` does not have `Cargo.lock`
    system "cargo", "generate-lockfile"
    system "cargo", "fetch", *std_cargo_fetch_args
  end

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
