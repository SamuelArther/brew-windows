class Emojify < Formula
  desc "Emoji on the command-line :scream:"
  homepage "https://github.com/mrowa44/emojify"
  url "https://github.com/mrowa44/emojify/archive/refs/tags/2.2.0.tar.gz"
  sha256 "340b866c432705989f71a61551c92af55f49f14d8f17b2c63a0db99e2d687e55"
  license "MIT"
  head "https://github.com/mrowa44/emojify.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "60feac2fedf265b52a89e8889553d9e7f029e3817eeccfca3baa505483c3f997"
  end

  on_macos do
    depends_on "bash"
  end

  deny_network_access!

  def install
    bin.install "emojify"
  end

  test do
    ENV["LC_ALL"] = "en_US.UTF-8"

    input = "Hey, I just :raising_hand: you, and this is :scream: , but here's my :calling: , " \
            "so :telephone_receiver: me, maybe?"
    assert_equal "Hey, I just 🙋 you, and this is 😱 , but here's my 📲 , so 📞 me, maybe?",
      shell_output("#{bin}/emojify \"#{input}\"").strip
  end
end
