class Kpt < Formula
  desc "Toolchain for composing, customizing, and deploying Kubernetes packages"
  homepage "https://kpt.dev"
  url "https://github.com/kptdev/kpt/archive/refs/tags/v1.0.1.tar.gz"
  sha256 "3c4c075d805c99a4fac0196c31ab770d9446852a5f328b6ced23514af46818d0"
  license "Apache-2.0"
  head "https://github.com/kptdev/kpt.git", branch: "main"

  livecheck do
    url :stable
    # Cannot use `github_latest` here as this might be "API" release
    strategy :github_releases do |json, regex|
      json.map do |release|
        next if release["draft"] || release["prerelease"]

        # Skip `api/*` releases
        next if release["name"]&.match?(/^api/i)

        match = release["tag_name"]&.match(regex)
        next if match.blank?

        match[1]
      end
    end
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "71d4fb82ae276086774bf74d7d1eb1771f7681c4fdc873d250a99c451ab3405c"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/kptdev/kpt/run.version=#{version}"
    system "go", "build", *std_go_args(ldflags:)
    generate_completions_from_executable(bin/"kpt", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/kpt version")

    (testpath/"pkg/Kptfile").write <<~YAML
      apiVersion: kpt.dev/v1
      kind: Kptfile
      metadata:
        name: example
    YAML
    (testpath/"pkg/deployment.yaml").write <<~YAML
      apiVersion: apps/v1
      kind: Deployment
      metadata:
        name: nginx
    YAML
    output = shell_output("#{bin}/kpt pkg tree #{testpath}/pkg")
    assert_match "Kptfile example", output
    assert_match "Deployment nginx", output
  end
end
