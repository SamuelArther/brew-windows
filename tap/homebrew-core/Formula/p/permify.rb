class Permify < Formula
  desc "Open-source authorization service & policy engine based on Google Zanzibar"
  homepage "https://permify.co/"
  url "https://github.com/Permify/permify/archive/refs/tags/v1.7.6.tar.gz"
  sha256 "9a1cc5b77e9929820b1e1ed10fa6d57e2c665bdd3fb0f755cb9a7499e147c37d"
  license "AGPL-3.0-only"
  head "https://github.com/Permify/permify.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f5388127b3fdc66c131f7edd525d9f792e3aef6f94781d742e79b4852076e143"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/permify"

    generate_completions_from_executable(bin/"permify", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/permify version")

    (testpath/"schema.yaml").write <<~YAML
      schema: >-
        entity user {}

        entity document {
          relation viewer @user
          action view = viewer
        }
    YAML

    # A drive letter (C:/...) reads as a URL scheme, so Windows passes the schema's relative path.
    output = shell_output("#{bin}/permify ast #{OS.windows? ? "schema.yaml" : "#{testpath}/schema.yaml"}")
    assert_equal "document", JSON.parse(output)["entityDefinitions"]["document"]["name"]
  end
end
