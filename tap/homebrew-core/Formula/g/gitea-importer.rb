class GiteaImporter < Formula
  desc "CLI to migrate repositories from GitHub, GitLab and Gitea into Gitea"
  homepage "https://gitea.com/gitea/importer"
  url "https://gitea.com/gitea/importer/archive/v0.5.0.tar.gz"
  sha256 "11c32c509b9ad471f9f00f54a9968acc830ee4e8510be30cd7a879d8b87f8ee1"
  license "MIT"
  head "https://gitea.com/gitea/importer.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "ee27fd95ff4f11429fbf6da35d6b791249f8bef901ab5659602c65810541f806"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X gitea.com/gitea/importer/internal/app.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gitea-importer version")

    output = shell_output("#{bin}/gitea-importer scan --source-type gitea --source-repos foo/bar 2>&1", 1)
    assert_match "gitea provider requires a token", output

    url = "http://127.0.0.1:#{free_port}"
    args = %W[--source-type gitea --source-url #{url} --source-auth pat://test --source-repos foo/bar]
    output = shell_output("#{bin}/gitea-importer scan #{args.join(" ")} 2>&1", 1)
    assert_match "Get \"#{url}/api/v1/", output
  end
end
