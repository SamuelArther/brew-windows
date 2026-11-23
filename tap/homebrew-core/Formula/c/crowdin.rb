class Crowdin < Formula
  desc "Command-line tool that allows to manage your resources with crowdin.com"
  homepage "https://support.crowdin.com/cli-tool/"
  url "https://github.com/crowdin/crowdin-cli/archive/refs/tags/5.6.0.tar.gz"
  sha256 "7092ca11e203904ee37a976c09ec99e31c09524ad0aa767d04433344d6f1306a"
  license "MIT"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "afd4c976d392ba9e1485faffcee11c3db653b663ac048a84188f15e9f1736438"
  end

  depends_on "bun" => :build

  on_linux do
    depends_on "icu4c@79"
  end

  deny_network_access! :test

  def install
    if OS.linux?
      bun_icu = Formula["bun"].deps("icu4c").first.to_formula
      icu = deps("icu4c").first.to_formula

      odie "Update icu4c dependency!" if bun_icu.name != icu.name
    end

    system "bun", "install", "--frozen-lockfile", "--ignore-scripts"
    system "bun", "run", "build"

    bin.install "dist/crowdin"
  end

  test do
    (testpath/"locale/en.json").write <<~JSON
      {"greeting": "Hello"}
    JSON

    (testpath/"crowdin.yml").write <<~YAML
      "project_id": "12"
      "api_token": "54e01--your-personal-token--2724a"
      "base_path": "."
      "base_url": "https://api.crowdin.com" # https://{organization-name}.crowdin.com

      "preserve_hierarchy": true

      "files": [
        {
          "source" : "/locale/*.json",
          "translation" : "/%two_letters_code%/%original_file_name%"
        }
      ]
    YAML

    assert_match "Your configuration file looks good",
      shell_output("#{bin}/crowdin config lint --config #{testpath}/crowdin.yml")

    rm testpath/"locale/en.json"

    assert_match "No source files found for '/locale/*.json' pattern",
      shell_output("#{bin}/crowdin config lint --config #{testpath}/crowdin.yml 2>&1", 2)
  end
end
