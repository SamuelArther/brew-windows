class Nelm < Formula
  desc "Kubernetes deployment tool that manages and deploys Helm Charts"
  homepage "https://github.com/werf/nelm"
  url "https://github.com/werf/nelm/archive/refs/tags/v1.27.0.tar.gz"
  sha256 "deb3878cbfd4889bba803cfe89ee34871b90ec4e283984972178344a7f43f514"
  license "Apache-2.0"
  head "https://github.com/werf/nelm.git", branch: "main"

  # Upstream may indicate an unstable release by marking it as pre-release,
  # appending an unstable suffix to the tag (e.g. "v1.2.3-alpha.1"), or
  # appending unstable text to the release title (e.g. "v1.2.3 [alpha]").
  # Unstable versions are not always marked as pre-release, so we have to check
  # multiple releases to reliably identify stable versions.
  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
    strategy :github_releases do |json, regex|
      json.map do |release|
        next if release["draft"] || release["prerelease"]

        next if release["name"]&.match?(/(alpha|beta|dev|rc)/i)

        match = release["tag_name"]&.match(regex)
        next if match.blank?

        match[1]
      end
    end
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "dc17dfd533fe7d434eaec2aa6a4b34f1f4ecd424df341e6d468fe8de88a4a3bb"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[-X github.com/werf/nelm/pkg/common.Version=#{version}]
    system "go", "build", *std_go_args(ldflags:), "./cmd/nelm"

    generate_completions_from_executable(bin/"nelm", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nelm version")

    (testpath/"Chart.yaml").write <<~YAML
      apiVersion: v2
      name: mychart
      version: 1.0.0
      dependencies:
      - name: cert-manager
        version: 1.13.3
        repository: https://127.0.0.1
    YAML
    assert_match "Error: no cached repository", shell_output("#{bin}/nelm chart dependency download 2>&1", 1)
  end
end
