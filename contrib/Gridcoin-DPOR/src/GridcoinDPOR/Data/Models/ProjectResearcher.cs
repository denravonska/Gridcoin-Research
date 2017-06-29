using System;
using System.Collections.Generic;
using System.Text;

namespace GridcoinDPOR.Data.Models
{
    public class ProjectResearcher
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; }
        public int ResearcherId { get; set; }
        public Researcher Reseracher { get; set; }
        public bool InTeam { get; set; }
        public double Credit { get; set; }
        public double RAC { get; set; }
        public double Mag { get; set; }
        public int WebUserId { get; set; }
    }
}
