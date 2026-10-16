class Goclone < Formula
  desc "Website Cloner"
  homepage "https://goclone.dev"
  url "https://github.com/goclone-dev/goclone/archive/refs/tags/v1.2.2.tar.gz"
  sha256 "1e005a045b3d2f5d4d0a7154f4552e537900c170256b668cc73aeac204d9defa"
  license "MIT"
  head "https://github.com/goclone-dev/goclone.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2f707525ba8ead73e85761fea8dd0826c1ab7469d17181eac01a5b724b2b05fc"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X sigs.k8s.io/release-utils/version.gitVersion=#{version}
      -X sigs.k8s.io/release-utils/version.gitCommit=#{tap.user}
      -X sigs.k8s.io/release-utils/version.buildDate=#{time.iso8601}
    ]
    system "go", "build", *std_go_args(ldflags:), "./cmd/goclone"

    generate_completions_from_executable(bin/"goclone", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/goclone version")

    system bin/"goclone", "https://example.com"
    assert_path_exists testpath/"example.com"
  end
end
