class Zbctl < Formula
  desc "Zeebe CLI client"
  homepage "https://github.com/camunda-community-hub/zeebe-client-go"
  url "https://github.com/camunda-community-hub/zeebe-client-go/archive/refs/tags/v8.7.1.tar.gz"
  sha256 "42fc26f985d91c9004ab6cf5a92940789ab0dd9516e7949a4097f1479194e73b"
  license "Apache-2.0"
  head "https://github.com/camunda-community-hub/zeebe-client-go.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "a0ed26635f3dfd847a8e33198ac983947d79267af5936e5a973ad540dcce5b03"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def install
    project = "github.com/camunda-community-hub/zeebe-client-go/v#{version.major}/cmd/zbctl/internal/commands"
    ldflags = "-X #{project}.Version=#{version} -X #{project}.Commit=#{tap.user}"
    system "go", "build", *std_go_args(ldflags:, tags: "netgo"), "./cmd/zbctl"

    generate_completions_from_executable(bin/"zbctl", shell_parameter_format: :cobra)
  end

  test do
    # Check status for a nonexistent cluster
    # (Windows words a refused connection "No connection could be made because the target machine actively refused it".)
    status_error_message =
      "Error: rpc error: code = " \
      "Unavailable desc = connection error: " \
      "desc = \"transport: Error while dialing: dial tcp 127.0.0.1:26500: " \
      "#{OS.windows? ? "connectex: No connection could be made" : "connect: connection refused"}"
    output = shell_output("#{bin}/zbctl status 2>&1", 1)
    assert_match status_error_message, output

    assert_match version.to_s, shell_output("#{bin}/zbctl version")
  end
end
