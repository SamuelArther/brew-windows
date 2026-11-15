class Goose < Formula
  desc "Go Language's command-line interface for database migrations"
  homepage "https://pressly.github.io/goose/"
  url "https://github.com/pressly/goose/archive/refs/tags/v3.30.1.tar.gz"
  sha256 "256e67dc83959a7febb27388d0f9b1efcc51bae5e53db31cb9bf20f3cfb468d2"
  license "MIT"
  head "https://github.com/pressly/goose.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d3253c720bb2297192a3099d9a71078ed7de1ad2214ec547e1e4da9f0cbbe91f"
  end

  depends_on "go" => :build

  conflicts_with "block-goose-cli", "goose-lang", because: "both install `goose` binaries"

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[-X main.version=v#{version}]
    system "go", "build", *std_go_args(ldflags:), "./cmd/goose"
  end

  test do
    output = shell_output("#{bin}/goose sqlite3 foo.db status create 2>&1", 1)
    assert_match "goose run: failed to collect migrations", output

    assert_match version.to_s, shell_output("#{bin}/goose --version")
  end
end
