class JiraCli < Formula
  desc "Feature-rich interactive Jira CLI"
  homepage "https://github.com/ankitpokhrel/jira-cli"
  url "https://github.com/ankitpokhrel/jira-cli/archive/refs/tags/v1.7.0.tar.gz"
  sha256 "6b1ecbd2228626cdc987548d8d83faae074c7a167cef737a9ac9180a03767154"
  license "MIT"
  head "https://github.com/ankitpokhrel/jira-cli.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "c950c7c2ae991aaf91047ed1b370a6a8513f6406dbe41cd00bed9e02229f0fa6"
  end

  depends_on "go" => :build

  conflicts_with "go-jira", because: "both install `jira` binaries"

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/ankitpokhrel/jira-cli/internal/version.Version=#{version}
      -X github.com/ankitpokhrel/jira-cli/internal/version.GitCommit=#{tap.user}
      -X github.com/ankitpokhrel/jira-cli/internal/version.SourceDateEpoch=#{time.to_i}
    ]
    system "go", "build", *std_go_args(ldflags:, output: bin/"jira"), "./cmd/jira"

    generate_completions_from_executable(bin/"jira", shell_parameter_format: :cobra)
    (man7/"jira.7").write Utils.safe_popen_read(bin/"jira", "man")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jira version")

    output = shell_output("#{bin}/jira serverinfo 2>&1", 1)
    assert_match "The tool needs a Jira API token to function", output
  end
end
