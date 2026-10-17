class Glasskube < Formula
  desc "Missing Package Manager for Kubernetes"
  homepage "https://glasskube.dev/products/package-manager/docs/"
  url "https://github.com/glasskube/glasskube/archive/refs/tags/v0.26.1.tar.gz"
  sha256 "c044187e49683b39aa89c26bc02dab38781578c24b6ab277c0a58ae811066996"
  license "Apache-2.0"
  head "https://github.com/glasskube/glasskube.git", branch: "main"

  deprecate! date: "2026-07-17", because: :repo_archived
  disable! date: "2027-01-17", because: :repo_archived

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3cda3ff4d3c9dc5ab9e80c318ad68bdec6663da059dd1cadda83881b48bc39f0"
  end

  depends_on "go" => :build
  depends_on "node" => :build

  def install
    ldflags = %W[
      -X github.com/glasskube/glasskube/internal/config.Version=#{version}
      -X github.com/glasskube/glasskube/internal/config.Commit=#{tap.user}
      -X github.com/glasskube/glasskube/internal/config.Date=#{time.iso8601}
    ]

    system "make", "web"
    system "go", "build", *std_go_args(ldflags:), "./cmd/glasskube"

    generate_completions_from_executable(bin/"glasskube", shell_parameter_format: :cobra)
  end

  test do
    output = shell_output("#{bin}/glasskube bootstrap --type slim 2>&1", 1)
    assert_match "Your kubeconfig file is either empty or missing!", output

    assert_match version.to_s, shell_output("#{bin}/glasskube --version")
  end
end
