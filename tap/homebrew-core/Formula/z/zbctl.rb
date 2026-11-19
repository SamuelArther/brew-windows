class Zbctl < Formula
  desc "Zeebe CLI client"
  homepage "https://github.com/camunda-community-hub/zeebe-client-go"
  url "https://github.com/camunda-community-hub/zeebe-client-go/archive/refs/tags/v8.7.0.tar.gz"
  sha256 "7798bd2bb8719b18e767acbd31ea7db69f0b170c799b5ef98aa988be2d06dddf"
  license "Apache-2.0"
  head "https://github.com/camunda-community-hub/zeebe-client-go.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "5784513583bae43ca6f1f21076f1bcdf708b5f58e1573cca2d40778b0bff93a5"
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
