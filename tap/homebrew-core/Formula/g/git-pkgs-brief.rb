class GitPkgsBrief < Formula
  desc "Tool that detects and reports a project's toolchain, configuration, and more"
  homepage "https://github.com/git-pkgs/brief"
  url "https://github.com/git-pkgs/brief/archive/refs/tags/v0.14.2.tar.gz"
  sha256 "e01028fc5bc79a49b76b0804325ea5b12796c3275799f82b89e4cc0717bc3bb5"
  license "MIT"
  head "https://github.com/git-pkgs/brief.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3a5eb19f5b36bca6e802b5775a633691d01b897895d3cb1404e9b7dfd6260566"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/git-pkgs/brief.Version=#{version}
    ]
    system "go", "build", *std_go_args(ldflags:, output: bin/"brief"), "./cmd/brief"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/brief -version")

    output = shell_output("#{bin}/brief https://github.com/Homebrew/brew")
    assert_match "license_type\": \"BSD-2-Clause\"", output
  end
end
