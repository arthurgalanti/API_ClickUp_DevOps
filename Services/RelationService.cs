using System.Threading.Tasks;
using apiClickupDevops.Models;
using MongoDB.Driver;

namespace apiClickupDevops.Services {
    public class RelationService {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Relation> _relations;

        public RelationService(MongoClient mongoClient) {
            _database = mongoClient.GetDatabase("ClickupDevopsRelation");
            _relations = _database.GetCollection<Relation>("Relations");
        }

        public async Task Create(Relation relation) {
            await _relations.InsertOneAsync(relation);
        }

        public async Task<Relation?> GetByClickupId(string id) {
            var relation = await _relations.FindAsync(relation => relation.Clickup.AppId == id);
            if (relation == null)
                return null;
            else
                return relation.FirstOrDefault();
        }

        public async Task<Relation?> GetByDevopsId(string id) {
            var relation = await _relations.FindAsync(relation => relation.Devops.AppId == id);
            return relation.FirstOrDefault();
        }
    }
}