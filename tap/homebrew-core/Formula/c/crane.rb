class Crane < Formula
  desc "Tool for interacting with remote images and registries"
  homepage "https://github.com/google/go-containerregistry"
  url "https://github.com/google/go-containerregistry/archive/refs/tags/v0.22.1.tar.gz"
  sha256 "a52cc7d61f8b2f043b7f0be1febecead5fceb791543c4790d699440f12d6b370"
  license "Apache-2.0"
  head "https://github.com/google/go-containerregistry.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "1a0fa6dd45df943c83d43380a13a0f656bc71d5499bbaea2475fe1508e3abd16"
  end

  depends_on "go" => :build

  # `test do` block fetches a manifest from gcr.io
  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[-X github.com/google/go-containerregistry/cmd/crane/cmd.Version=#{version}]

    system "go", "build", *std_go_args(ldflags:), "./cmd/crane"

    generate_completions_from_executable(bin/"crane", shell_parameter_format: :cobra)
  end

  test do
    json_output = shell_output("#{bin}/crane manifest gcr.io/go-containerregistry/crane")
    manifest = JSON.parse(json_output)
    assert_equal manifest["schemaVersion"], 2
  end
end
