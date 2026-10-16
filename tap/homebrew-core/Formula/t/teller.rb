class Teller < Formula
  desc "Secrets management tool for developers"
  homepage "https://github.com/tellerops/teller"
  url "https://github.com/tellerops/teller/releases/download/v2.0.7/teller-x86_64-windows.zip"
  sha256 "44d03d51a065c71bf9228effdc985017983f7d51738fa437c1076cbf1a2c3853"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tellerops/teller/archive/refs/tags/v2.0.7.tar.gz"
  end

  def install
    bin.install "teller.exe"
  end

  test do
    (testpath/"test.env").write <<~EOS
      foo=bar
    EOS

    (testpath/".teller.yml").write <<~YAML
      project: brewtest
      providers:
        # this will fuse vars with the below .env file
        # use if you'd like to grab secrets from outside of the project tree
        dotenv:
          kind: dotenv
          maps:
          - id: one
            path: #{testpath}/test.env
    YAML

    output = shell_output("#{bin}/teller -c #{testpath}/.teller.yml show 2>&1")
    assert_match "[dotenv (dotenv)]: foo = ba", output

    assert_match version.to_s, shell_output("#{bin}/teller --version")
  end
end
