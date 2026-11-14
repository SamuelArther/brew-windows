class Evnx < Formula
  desc "Comprehensive CLI tool for managing .env files"
  homepage "https://evnx.dev"
  url "https://github.com/urwithajit9/evnx/archive/refs/tags/v0.14.1.tar.gz"
  sha256 "45e83c082688e1a471af999ad5590e2e1f69a9d3001e8553b7720e214b9c81f4"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "67d23641d466527c66e89f254f31f1945a80341ce8e34f5e5a9f0af4265b3714"
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
    assert_match version.to_s, shell_output("#{bin}/evnx --version")

    system bin/"evnx", "init", "--yes"
    assert_path_exists testpath/".env"
    assert_match "All checks passed", shell_output("#{bin}/evnx validate")

    (testpath/".env.example").append_lines "API_KEY="
    assert_match "Validation failed", shell_output("#{bin}/evnx validate 2>&1", 1)
  end
end
