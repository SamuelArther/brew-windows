class Comtrya < Formula
  desc "Configuration and dotfile management tool"
  homepage "https://comtrya.dev"
  url "https://github.com/comtrya/comtrya/archive/refs/tags/v0.9.2.tar.gz"
  sha256 "7bbd6ac04314e2dd541d8104908a2e8991a7489daab5563d01d28a3e48c08350"
  license "MIT"
  head "https://github.com/comtrya/comtrya.git", branch: "main"

  deprecate! date: "2026-07-09", because: :unmaintained
  disable! date: "2027-07-09", because: :unmaintained

  bottle do
    sha256 cellar: :any, x86_64_windows: "767684422c8b4e0e55a8ca0ffa0821dea527ae60b553fcf59548370f298ca91f"
  end

  depends_on "rust" => :build

  resource "testmanifest", :test do
    url "https://raw.githubusercontent.com/comtrya/comtrya/refs/heads/main/examples/onlyvariants/main.yaml"
    sha256 "0715e12cbbb95c8d6c36bb02ae4b49f9fa479e2f28356b8c1f3b5adfb000b93f"
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "app")

    generate_completions_from_executable(bin/"comtrya", "gen-completions")
  end

  test do
    assert_match "comtrya #{version}", shell_output("#{bin}/comtrya --version")

    resource("testmanifest").stage do
      system bin/"comtrya", "-d", "main.yaml", "apply"
    end
  end
end
