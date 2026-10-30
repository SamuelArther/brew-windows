class Appwrite < Formula
  desc "Command-line tool for Appwrite"
  homepage "https://appwrite.io"
  url "https://github.com/appwrite/sdk-for-cli/archive/refs/tags/28.10.1.tar.gz"
  sha256 "f5f845aaa49bf45bf465b07ae759ed8d38cde441d30ff5666f007f07ca21ce08"
  license "BSD-3-Clause"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "645d101196f4ee29e2a18e0e4bbf544d1b0286916edc2b18251fa241fd21d920"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    # `go mod tidy` needs module metadata beyond what `go mod download` fetches
    system "go", "mod", "tidy"
  end

  def install
    # https://github.com/appwrite/sdk-for-cli/blob/4399a3321898f40cf982acbd4859d506c9d4d9f4/.goreleaser.yaml#L19-L22
    system "go", "mod", "tidy"
    system "go", "build", *std_go_args(ldflags: "-X github.com/appwrite/sdk-for-cli/internal/app.Version=#{version}")

    generate_completions_from_executable(bin/"appwrite", "completion")
  end

  test do
    output = shell_output("#{bin}/appwrite client --endpoint http://localhost/v1 2>&1", 1)
    assert_match "Error: invalid endpoint", output

    assert_match version.to_s, shell_output("#{bin}/appwrite --version")
  end
end
