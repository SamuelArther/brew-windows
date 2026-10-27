class Fastly < Formula
  desc "Build, deploy and configure Fastly services"
  homepage "https://www.fastly.com/documentation/reference/cli/"
  url "https://github.com/fastly/cli/archive/refs/tags/v17.0.0.tar.gz"
  sha256 "4e9056f72509a375afb30c07883ea738434bb5b2ae0b2a8d5d0dd776101463e5"
  license "Apache-2.0"
  head "https://github.com/fastly/cli.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "4fa2023f66e4646ebb05014347cd861df43090c6181994b57e6bf898e38ff935"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    os = Utils.safe_popen_read("go", "env", "GOOS").strip
    arch = Utils.safe_popen_read("go", "env", "GOARCH").strip

    ldflags = %W[
      -X github.com/fastly/cli/pkg/revision.AppVersion=v#{version}
      -X github.com/fastly/cli/pkg/revision.GitCommit=#{tap.user}
      -X github.com/fastly/cli/pkg/revision.GoHostOS=#{os}
      -X github.com/fastly/cli/pkg/revision.GoHostArch=#{arch}
      -X github.com/fastly/cli/pkg/revision.Environment=release
    ]

    system "go", "build", *std_go_args(ldflags:), "./cmd/fastly"

    generate_completions_from_executable(bin/"fastly", shell_parameter_format: "--completion-script-",
                                                       shells:                 [:bash, :zsh])
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fastly version")

    ENV["FASTLY_API_TOKEN"] = "invalid-token"
    output = shell_output("#{bin}/fastly service list 2>&1", 1)
    assert_match "401 Unauthorized", output
  end
end
