class Flank < Formula
  desc "Massively parallel Android and iOS test runner for Firebase Test Lab"
  homepage "https://firebase.community/"
  url "https://github.com/Flank/flank/releases/download/v23.10.1/flank.jar"
  sha256 "719ba0ca5744f571aad01fc61392b18990833ee9dd36e6b600ccdff614350d58"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "90bc3e99ef299c4b5006990d4aefd121505ac6643333a4608c70c22522fb100c"
  end

  depends_on "openjdk"

  def install
    libexec.install "flank.jar"
    bin.write_jar_script libexec/"flank.jar", "flank"
  end

  test do
    (testpath/"flank.yml").write <<~YAML
      gcloud:
        device:
        - model: Pixel2
          version: "29"
          locale: en
          orientation: portrait
    YAML

    output = shell_output("#{bin}/flank android doctor")
    assert_match "Valid yml file", output
  end
end
