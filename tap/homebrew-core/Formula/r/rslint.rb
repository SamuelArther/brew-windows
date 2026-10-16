class Rslint < Formula
  desc "Extremely fast JavaScript and TypeScript linter"
  homepage "https://rslint.org/"
  url "https://github.com/rslint/rslint/archive/refs/tags/v0.3.2.tar.gz"
  sha256 "1f3388c0bedb5bfb4c83f49ab773de91652144cb08327038272ad715cb161a83"
  license "MIT"
  head "https://github.com/rslint/rslint.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "9f927342380831f706dec2eb956b453fc055779566debbf7bee5e257fb366579"
  end

  depends_on "rust" => :build

  # Fix to error: implicit autoref creates a reference to the dereference of a raw pointer
  patch do
    url "https://github.com/rslint/rslint/commit/1ceaa3a78d591599a5fe30f702bdb3dae1004f70.patch?full_index=1"
    sha256 "31186aec32f3a587f6401e2610ffa0dec2b37adc1bc4d9a88f816d87f57aa5a4"
    type :unofficial
    resolves "https://github.com/rslint/rslint/pull/165"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "crates/rslint_cli")
  end

  test do
    # https://rslint.org/rules/errors/no-empty.html#invalid-code-examples
    (testpath/"no-empty.js").write("{}")
    output = shell_output("#{bin}/rslint no-empty.js 2>&1", 1)
    assert_match "1 fail, 0 warn, 0 success", output
    assert_match "empty block statements are not allowed", output
  end
end
