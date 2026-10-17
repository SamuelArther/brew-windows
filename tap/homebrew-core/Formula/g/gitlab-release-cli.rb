class GitlabReleaseCli < Formula
  desc "Toolset to create, retrieve and update releases on GitLab"
  homepage "https://gitlab.com/gitlab-org/release-cli"
  url "https://gitlab.com/gitlab-org/release-cli/-/archive/v0.24.0/release-cli-v0.24.0.tar.bz2"
  sha256 "e13a53fdaf60e2fb2df620969dd35131800cddad8d3d4c476154110249629daa"
  license "MIT"
  head "https://gitlab.com/gitlab-org/release-cli.git", branch: "master"

  deprecate! date: "2026-07-17", because: :deprecated_upstream, replacement_formula: "glab"
  disable! date: "2027-01-17", because: :deprecated_upstream, replacement_formula: "glab"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "135e4e09a47117de8f49158529939451cc6503d9fa681df04ff6e52493e4d717"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X main.VERSION=#{version}"
    system "go", "build", *std_go_args(ldflags:, output: bin/"release-cli"), "./cmd/release-cli"
  end

  test do
    ENV["CI_SERVER_URL"] = "https://gitlab.com"
    ENV["CI_PROJECT_ID"] = "12345678"

    assert_match version.to_s, shell_output("#{bin}/release-cli --version")

    output = shell_output("#{bin}/release-cli create --tag-name v1.0.0 2>&1", 1)
    assert_match "failed to create GitLab client: access token not provided", output
  end
end
