class Ryl < Formula
  desc "Fast YAML linter and formatter written in Rust"
  homepage "https://ryl-docs.pages.dev"
  url "https://github.com/owenlamont/ryl/archive/refs/tags/v0.26.1.tar.gz"
  sha256 "062cc0342d33bcce4a105bf22c96d44df862306d03d926a83c2922d2b8329834"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "9766f9a4f9ffd52beac82119bfb7fa9f8b676725d1d3be132ceaa0fac37d23ba"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
    generate_completions_from_executable(bin/"ryl", "--generate-completions")
  end

  test do
    (testpath/"valid.yaml").write <<~YAML
      ---
      ...
    YAML
    system bin/"ryl", "check", "--enable", "ALL", "valid.yaml"
  end
end
