class K3d < Formula
  desc "Little helper to run CNCF's k3s in Docker"
  homepage "https://k3d.io"
  url "https://github.com/k3d-io/k3d/archive/refs/tags/v5.9.0.tar.gz"
  sha256 "969cce82c4871bb829be798655abe2b6709b77cc0f42f7ff69293c621dfbbab0"
  license "MIT"
  head "https://github.com/k3d-io/k3d.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "22bd5cd41c8675600e27619254e83d88d0c3f6202237334f6be9a1bc1a724805"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    require "net/http"
    uri = URI("https://update.k3s.io/v1-release/channels")
    resp = Net::HTTP.get(uri)
    resp_json = JSON.parse(resp)
    k3s_version = resp_json["data"].find { |channel| channel["id"]=="stable" }["latest"].sub("+", "-")
    raise "Invalid k3s version" unless k3s_version.match?(/\Av\d+\.\d+\.\d+-k3s\d+\z/)

    (buildpath/"k3s_version").write k3s_version
    # Resolve the dependencies the same way `install` builds (`-mod=readonly`, which
    # differs from the workspace graph); `go mod download` trips over unresolved
    # `v0.0.0` requirements of the workspace's other modules.
    system "go", "list", "-mod=readonly", "-deps", "."
  end

  def install
    k3s_version = (buildpath/"k3s_version").read

    ldflags = %W[
      -X github.com/k3d-io/k3d/v#{version.major}/version.Version=v#{version}
      -X github.com/k3d-io/k3d/v#{version.major}/version.K3sVersion=#{k3s_version}
    ]

    system "go", "build", "-mod=readonly", *std_go_args(ldflags:)

    generate_completions_from_executable(bin/"k3d", shell_parameter_format: :cobra)
  end

  test do
    assert_match "k3d version v#{version}", shell_output("#{bin}/k3d version")
    # Either docker is not present or it is, where the command will fail in the first case.
    # In any case I wouldn't expect a cluster with name 6d6de430dbd8080d690758a4b5d57c86 to be present
    # (which is the md5sum of 'homebrew-failing-test')
    output = shell_output("#{bin}/k3d cluster get 6d6de430dbd8080d690758a4b5d57c86 2>&1", 1).split("\n").pop
    assert_match "No nodes found for given cluster", output
  end
end
