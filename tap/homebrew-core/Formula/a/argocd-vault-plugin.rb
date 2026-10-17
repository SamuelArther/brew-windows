class ArgocdVaultPlugin < Formula
  desc "Argo CD plugin to retrieve secrets from Secret Management tools"
  homepage "https://argocd-vault-plugin.readthedocs.io"
  url "https://github.com/argoproj-labs/argocd-vault-plugin/archive/refs/tags/v1.18.1.tar.gz"
  sha256 "687141d21c1baaedab6b6d4f5e11e72faf6eec7f7d889be7095a48a888080e32"
  license "Apache-2.0"
  head "https://github.com/argoproj-labs/argocd-vault-plugin.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2b4ef6931663eae3556f86af8fdb0d58c20c4aa96bf64ed8e4c895a0023065a1"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = "0"

    commit = build.head? ? Utils.git_head : Utils::Git.get_tar_commit_id(cached_download)
    ldflags = %W[
      -X github.com/argoproj-labs/argocd-vault-plugin/version.Version=#{version}
      -X github.com/argoproj-labs/argocd-vault-plugin/version.BuildDate=#{time.iso8601}
      -X github.com/argoproj-labs/argocd-vault-plugin/version.CommitSHA=#{commit}
    ]

    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable(bin/"argocd-vault-plugin", shell_parameter_format: :cobra)
  end

  test do
    assert_match "This is a plugin to replace <placeholders> with Vault secrets",
      shell_output("#{bin}/argocd-vault-plugin --help")

    touch testpath/"empty.yaml"
    assert_match "Error: Must provide a supported Vault Type",
      shell_output("#{bin}/argocd-vault-plugin generate ./empty.yaml 2>&1", 1)
  end
end
