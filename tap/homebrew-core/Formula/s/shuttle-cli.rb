class ShuttleCli < Formula
  desc "CLI for handling shared build and deploy tools between many projects"
  homepage "https://github.com/lunarway/shuttle"
  url "https://github.com/lunarway/shuttle/archive/refs/tags/v0.25.1.tar.gz"
  sha256 "7112b1b562aee1f2b9af17362aa18891a02571ab405d64719fb67ce887a3c5a3"
  license "Apache-2.0"
  head "https://github.com/lunarway/shuttle.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "a3f2d9513f0dbb6b2e568b8083feec835b4d368ac2d33f1f395164116a7707bc"
  end

  depends_on "go" => :build

  conflicts_with "cargo-shuttle", because: "both install `shuttle` binaries"

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/lunarway/shuttle/cmd.version=#{version}
      -X github.com/lunarway/shuttle/cmd.commit=#{tap.user}
    ]
    system "go", "build", *std_go_args(output: bin/"shuttle", ldflags:)

    generate_completions_from_executable(bin/"shuttle", "completion", shells: [:bash, :zsh])
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/shuttle version")

    (testpath/"shuttle.yaml").write <<~YAML
      plan: 'https://github.com/lunarway/shuttle-example-go-plan.git'
      vars:
        docker:
          baseImage: golang
          baseTag: stretch
          destImage: repo-project
          destTag: latest
    YAML

    assert_match "Plan:", shell_output("#{bin}/shuttle config")

    output = shell_output("#{bin}/shuttle telemetry upload 2>&1", 1)
    assert_match "SHUTTLE_REMOTE_TRACING_URL or upload-url is not set", output
  end
end
